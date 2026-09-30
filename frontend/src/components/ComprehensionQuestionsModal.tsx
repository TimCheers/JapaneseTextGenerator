import { useContext, useEffect, useState } from "react";
import { AuthContext } from "../context/AuthContext";
import { Modal } from "./Modal";
import type { PracticeAttempt } from "../api/practiceAttempts";
import { createPracticeAttempt, completePracticeAttempt } from "../api/practiceAttempts";
import type { ComprehensionQuestion } from "../api/comprehensionQuestions";
import { getComprehensionQuestions } from "../api/comprehensionQuestions";
import type { UserAnswer } from "../api/userAnswers";
import { submitAnswer } from "../api/userAnswers";
import "../styles/ComprehensionQuestionsModal.css";

interface ComprehensionQuestionsModalProps {
    isOpen: boolean;
    onClose: () => void;
    generatedTextId: string;
}

export function ComprehensionQuestionsModal({ isOpen, onClose, generatedTextId }: ComprehensionQuestionsModalProps) {
    const { user } = useContext(AuthContext);
    const [attempt, setAttempt] = useState<PracticeAttempt | null>(null);
    const [questions, setQuestions] = useState<ComprehensionQuestion[]>([]);
    const [selected, setSelected] = useState<Record<string, string>>({});
    const [answers, setAnswers] = useState<Record<string, UserAnswer>>({});
    const [completed, setCompleted] = useState<PracticeAttempt | null>(null);
    const [error, setError] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        if (!isOpen || !user) return;

        setAttempt(null);
        setQuestions([]);
        setSelected({});
        setAnswers({});
        setCompleted(null);
        setError("");
        setIsLoading(true);

        Promise.all([
            createPracticeAttempt(user.id, generatedTextId),
            getComprehensionQuestions(user.id, generatedTextId),
        ])
            .then(([newAttempt, loadedQuestions]) => {
                setAttempt(newAttempt);
                setQuestions(loadedQuestions);
            })
            .catch((err) => {
                setError(err instanceof Error ? err.message : "Something went wrong");
            })
            .finally(() => setIsLoading(false));
    }, [isOpen, generatedTextId, user]);

    function handleSelect(questionId: string, option: string) {
        if (answers[questionId]) return;
        setSelected((prev) => ({ ...prev, [questionId]: option }));
    }

    async function handleAnswer(questionId: string) {
        if (!user || !attempt || !selected[questionId]) return;
        try {
            const result = await submitAnswer(user.id, attempt.id, questionId, selected[questionId]);
            setAnswers((prev) => ({ ...prev, [questionId]: result }));
        } catch (err) {
            setError(err instanceof Error ? err.message : "Something went wrong");
        }
    }

    async function handleComplete() {
        if (!user || !attempt) return;
        try {
            const result = await completePracticeAttempt(user.id, attempt.id);
            setCompleted(result);
        } catch (err) {
            setError(err instanceof Error ? err.message : "Something went wrong");
        }
    }

    const allAnswered = questions.length > 0 && questions.every((q) => answers[q.id]);

    return (
        <Modal isOpen={isOpen} onClose={onClose}>
            <div className="comprehension-modal">
                <h2>Comprehension Check</h2>
                {isLoading && <p>Louding...</p>}
                {error && <p className="comprehension-error">{error}</p>}
                {questions.map((question) => {
                    const answer = answers[question.id];
                    return (
                        <div key={question.id} className="comprehension-question">
                            <p>{question.questionText}</p>
                            {question.options.map((option) => (
                                <label key={option} className="comprehension-option">
                                    <input
                                        type="radio"
                                        name={question.id}
                                        checked={selected[question.id] === option}
                                        disabled={!!answer}
                                        onChange={() => handleSelect(question.id, option)}
                                    />
                                    {option}
                                </label>
                            ))}
                            {!answer ? (
                                <button disabled={!selected[question.id]} onClick={() => handleAnswer(question.id)}>
                                    Answer
                                </button>
                            ) : (
                                <p className={answer.isCorrect ? "comprehension-correct" : "comprehension-incorrect"}>
                                    {answer.isCorrect ? "Correct!" : "Uncorrct"}
                                </p>
                            )}
                        </div>
                    );
                })}
                {allAnswered && !completed && <button onClick={handleComplete}>Done</button>}
                {completed && (
                    <p className="comprehension-score">
                        Result: {completed.score} from {questions.length}
                    </p>
                )}
            </div>
        </Modal>
    );
}