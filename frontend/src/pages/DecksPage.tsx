import { useState, useEffect, useContext } from "react"
import type { Deck } from "../api/decks"
import { getDecks } from "../api/decks"
import { AuthContext } from "../context/AuthContext"
import { createDeck } from "../api/decks"
import type { CreateDeckRequest } from "../api/decks"
import { DeckCard } from "../components/DeckCard"
import "../styles/DeckForm.css";


export function DecksPage() {
    const { user } = useContext(AuthContext);
    const [decks, setDecks] = useState<Deck[]>([]);
    const [form, setForm] = useState<CreateDeckRequest>({
        name: "",
        description: ""
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm({ ...form, [name]: value });
    };

    useEffect(() => {
        if (user) {
            getDecks(user.id).then((result) => setDecks(result));
        }
    }, [user]);
    
    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            if (!user) return;
            const newDeck = await createDeck(user.id, form);
            setDecks([...decks, newDeck]);
            setForm({
                name: "",
                description: ""
            });
            setSuccess(true);
        } catch (err) {
            setError("Something went wrong");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <h1>My Decks</h1>
            <form onSubmit={handleSubmit} className="deck-form">
                <input placeholder="name" name="name" value={form.name} onChange={handleChange} />
                <input placeholder="description"
                    name="description"
                    value={form.description ?? ""}
                    onChange={handleChange} />
                <button type="submit">Create</button>
                {error && <p>{error}</p>}
            </form>
            <div className="decks-list">
                {decks.map((deck) => (
                    <DeckCard key={deck.id} deck={deck} />
                ))}
            </div>
        </div>
    );
}