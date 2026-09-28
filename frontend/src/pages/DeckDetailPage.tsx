import { useParams } from "react-router-dom";
import { useState, useEffect, useContext } from "react"
import { importWordsFromExcel, getWordsForDeck, createWord, type Word } from "../api/words"
import { AuthContext } from "../context/AuthContext"
import { getDeckById, updateDeck, type Deck, type CreateDeckRequest, deleteDeck } from "../api/decks"
import { type CreateWordRequest, deleteWord, updateWord } from "../api/words"
import { useNavigate } from "react-router-dom";
import { Modal } from "../components/Modal";
import "../styles/DeckForm.css";



export function DeckDetailPage() {
    const navigate = useNavigate();
    const { deckId } = useParams<{ deckId: string }>();
    const [words, setWords] = useState<Word[]>([]);
    const [file, setFile] = useState<File | null>(null);
    const [error, setError] = useState("");
    const { user } = useContext(AuthContext);
    const [deck, setDeck] = useState<Deck | null>(null);
    const [editingWord, setEditingWord] = useState<Word | null>(null);
    const [isDeckModalOpen, setIsDeckModalOpen] = useState(false);
    const [isWordModalOpen, setIsWordModalOpen] = useState(false);
    const [form, setForm] = useState<CreateDeckRequest>({
        name: "",
        description: ""
    });
    const [wordForm, setWordForm] = useState<CreateWordRequest>({
        term: "",
        reading: "",
        meaning: "",
        partOfSpeech: null,
        jlptLevel: null,
        exampleSentence: "",
        notes: null,
        acquisitionSource: null,
    });

    useEffect(() => {
        if (user && deckId) {
            getDeckById(user.id, deckId).then((result) => setDeck(result));
        }
    }, [user, deckId]);

    useEffect(() => {
        if (deck) {
            setForm({ name: deck.name, description: deck.description });
        }
    }, [deck]);

    useEffect(() => {
        if (deckId) {
            getWordsForDeck(deckId).then((result) => setWords(result));
        }
    }, [deckId]);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFile(e.target.files?.[0] ?? null);
    };

    const handleUploadWords = async () => {
        if (file && deckId) {
            try {
                const importedWords = await importWordsFromExcel(deckId, file);
                setWords([...words, ...importedWords]);
            } catch (err) {
                setError(err instanceof Error ? err.message : "Something went wrong");
            }

        }
    };

    const handleDeleteDeck = async () => {
        if (user && deck) {
            try {
                await deleteDeck(user.id, deck.id);
                navigate("/decks");
            } catch (err) {
                setError(err instanceof Error ? err.message : "Something went wrong");
            }

        }
    };

    const handleUpdateDeck = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        try {
            if (deck && user) {
                await updateDeck(user.id, deck.id, form);
                setDeck({ ...deck, ...form });
                setIsDeckModalOpen(false);
            }
        } catch (err) {
            setError("Something went wrong");
        }
    };

    const handleChangeDack = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm({ ...form, [name]: value });
    };

    const handleAddWord = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        try {
            if (deckId) {
                if (editingWord) {
                    await updateWord(deckId, editingWord.id, wordForm);
                    setWords(words.map(w => w.id === editingWord.id ? { ...w, ...wordForm } : w));
                }
                else {
                    const createdWord = await createWord(deckId, wordForm);
                    setWords([...words, createdWord]);
                }
                setWordForm({
                    term: "",
                    reading: "",
                    meaning: "",
                    partOfSpeech: null,
                    jlptLevel: null,
                    exampleSentence: "",
                    notes: null,
                    acquisitionSource: null,
                });
                setIsWordModalOpen(false);
                setEditingWord(null);
            }
        } catch (err) {
            setError("Something went wrong");
        }
    }

    const handleChangeWord = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setWordForm({ ...wordForm, [name]: value });
    };

    const handleDeleteWord = async (wordId: string) => {
        if (deck) {
            try {
                await deleteWord(deck.id, wordId);
                setWords(words.filter(x => x.id != wordId));
            } catch (err) {
                setError(err instanceof Error ? err.message : "Something went wrong");
            }

        }
    };

    const handleEditeDeckW = () => {
        setIsDeckModalOpen(true);
    };

    const handleEditeWordW = () => {
        setIsWordModalOpen(true);
        setWordForm({
            term: "",
            reading: "",
            meaning: "",
            partOfSpeech: null,
            jlptLevel: null,
            exampleSentence: "",
            notes: null,
            acquisitionSource: null,
        });
        setEditingWord(null);
    };

    const handleWordClick = (word: Word) => {
        setEditingWord(word);
        setWordForm({
            term: word.term,
            reading: word.reading,
            meaning: word.meaning,
            partOfSpeech: word.partOfSpeech,
            jlptLevel: word.jlptLevel,
            exampleSentence: word.exampleSentence,
            notes: word.notes,
            acquisitionSource: word.acquisitionSource,
        });
        setIsWordModalOpen(true);
    };

    return (
        <>
            <div className="import-section">
                <p>Import words</p>
                <div className="upload-form">
                    <input type="file" onChange={handleFileChange} />
                    <button onClick={handleUploadWords}>Upload</button>
                </div>
            </div>
            <div>
                <Modal isOpen={isDeckModalOpen} onClose={() => setIsDeckModalOpen(false)}>
                    <form onSubmit={handleUpdateDeck} className="deck-form">
                        <input placeholder="name"
                            name="name"
                            value={form.name ?? ""} onChange={handleChangeDack} />
                        <input placeholder="description"
                            name="description"
                            value={form.description ?? ""} onChange={handleChangeDack} />
                        <button type="submit">Update</button>
                        {error && <p>{error}</p>}
                    </form>
                </Modal>
                <Modal isOpen={isWordModalOpen} onClose={() => setIsWordModalOpen(false)}>
                    <form onSubmit={handleAddWord} className="word-form">
                        <input placeholder="word"
                            name="term"
                            value={wordForm.term ?? ""} onChange={handleChangeWord} />
                        <input placeholder="reading"
                            name="reading"
                            value={wordForm.reading ?? ""} onChange={handleChangeWord} />
                        <input placeholder="meaning"
                            name="meaning"
                            value={wordForm.meaning ?? ""} onChange={handleChangeWord} />
                        <input placeholder="partOfSpeech"
                            name="partOfSpeech"
                            value={wordForm.partOfSpeech ?? ""} onChange={handleChangeWord} />
                        <input placeholder="jlptLevel"
                            name="jlptLevel"
                            value={wordForm.jlptLevel ?? ""} onChange={handleChangeWord} />
                        <input placeholder="exampleSentence"
                            name="exampleSentence"
                            value={wordForm.exampleSentence ?? ""} onChange={handleChangeWord} />
                        <input placeholder="notes"
                            name="notes"
                            value={wordForm.notes ?? ""} onChange={handleChangeWord} />
                        <input placeholder="acquisitionSource"
                            name="acquisitionSource"
                            value={wordForm.acquisitionSource ?? ""} onChange={handleChangeWord} />
                        <button type="submit">Done</button>
                        {error && <p>{error}</p>}
                    </form>
                </Modal>
                <div className="deck-detail-header">
                    <div>
                        <h2>{deck?.name}</h2>
                        <h4>{deck?.description}</h4>
                    </div>
                    <div className="deck-actions">
                        <button type="button" onClick={handleDeleteDeck}>Delete deck</button>
                        <button type="button" onClick={handleEditeDeckW}>Edite deck</button>
                        <button type="button" onClick={handleEditeWordW}>Add word</button>
                    </div>
                </div>
                <table className="words-table">
                    <thead>
                        <tr>
                            <th>Word</th>
                            <th>Reading</th>
                            <th>Meaning</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {words.map((word) => (
                            <tr key={word.id} onClick={() => handleWordClick(word)}>
                                <td>{word.term}</td>
                                <td>{word.reading}</td>
                                <td>{word.meaning}</td>
                                <td>
                                    <button
                                        type="button"
                                        className="delete-word-btn"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleDeleteWord(word.id);
                                        }}
                                    >
                                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
                                            <path d="M3 6h18" />
                                            <path d="M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2" />
                                            <path d="M19 6l-1 14a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1L5 6" />
                                            <path d="M10 11v6" />
                                            <path d="M14 11v6" />
                                        </svg>
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div >
        </>
    );
}
