import { useState, useEffect, useContext } from "react"
import type { Deck } from "../api/decks"
import { getDecks } from "../api/decks"
import { AuthContext } from "../context/AuthContext"

export function DecksPage() {
    const { user } = useContext(AuthContext);
    const [decks, setDecks] = useState<Deck[]>([]);

    useEffect(() => {
        if (user) {
            getDecks(user.id).then((result) => setDecks(result));
        }
    }, [user]);

    return (
        <div>
            <h1>My Decks</h1>
            <ul>
                {decks.map((deck) => (
                    <li key={deck.id}>{deck.name}</li>
                ))}
            </ul>
        </div>
    );
}