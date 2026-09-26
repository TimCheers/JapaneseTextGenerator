import type { Deck } from "../api/decks"
import "../styles/DeckCard.css";


interface DeckCardProps {
    deck: Deck;
}

export function DeckCard({ deck }: DeckCardProps) {

    return (
        <div className="deck-card">
            <p>{deck.name}</p>
            <p>{deck.description}</p>
        </div>
    );
}