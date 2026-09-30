import type { GeneratedText } from "../api/generatedTexts"
import { ComprehensionQuestionsModal } from "./ComprehensionQuestionsModal";
import "../styles/TextCard.css";
import { useState } from "react";


interface TextCardProps {
    text: GeneratedText;
}

export function TextCard({ text }: TextCardProps) {
    const [isModalOpen, setIsModalOpen] = useState(false);

    return (
        <div className="text-card">
            <p>{text.content}</p>
            <button onClick={() => setIsModalOpen(true)}>Comprehension Check</button>
            <ComprehensionQuestionsModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                generatedTextId={text.id}
            />
        </div>
    );
}