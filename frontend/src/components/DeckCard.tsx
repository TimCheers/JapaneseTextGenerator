import type { Deck } from "../api/decks"
import { Link } from "react-router-dom";
import "../styles/DeckCard.css";


interface DeckCardProps {
    deck: Deck;
}

export function DeckCard({ deck }: DeckCardProps) {

    return (
        <Link to={`/decks/${deck.id}`}>
            <div className="deck-card">
                <p>{deck.name}</p>
                <p>{deck.description}</p>
            </div>
        </Link>
    );
}