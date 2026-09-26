import type { GeneratedText } from "../api/generatedTexts"
import "../styles/TextCard.css";


interface TextCardProps {
    text: GeneratedText;
}

export function TextCard({ text }: TextCardProps) {

    return (
        <div className="text-card">
            <p>{text.content}</p>
        </div>
    );
}