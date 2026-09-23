import type { GeneratedText } from "../types/generatedText";
import { TextCard } from "./TextCard";
import "./TextFeed.css";

interface TextFeedProps {
  texts: GeneratedText[];
}

export function TextFeed({ texts }: TextFeedProps) {
  return (
    <div className="text-feed">
      {texts.map((text) => (
        <TextCard key={text.id} text={text} />
      ))}
    </div>
  );
}