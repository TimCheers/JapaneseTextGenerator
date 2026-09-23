import type { GeneratedText } from "../types/generatedText";
import "./TextCard.css";

interface TextCardProps {
  text: GeneratedText;
}

export function TextCard({ text }: TextCardProps) {
  return (
    <section className="text-card">
      <p className="text-card__content">{text.content}</p>
    </section>
  );
}