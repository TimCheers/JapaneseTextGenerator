import { useParams } from "react-router-dom";
import { useState, useEffect, useContext } from "react"
import { importWordsFromExcel } from "../api/words"
import { getWordsForDeck } from "../api/words"
import { AuthContext } from "../context/AuthContext"
import type { Word } from "../api/words"
import type { CreateDeckRequest } from "../api/decks"

import { getDeckById, updateDeck, type Deck } from "../api/decks"


export function DeckDetailPage() {
    const { deckId } = useParams<{ deckId: string }>();
    const [words, setWords] = useState<Word[]>([]);
    const [file, setFile] = useState<File | null>(null);
    const [error, setError] = useState("");
    const { user } = useContext(AuthContext);
    const [deck, setDeck] = useState<Deck | null>(null);
    const [form, setForm] = useState<CreateDeckRequest>({
        name: "",
        description: ""
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

    const handleUpload = async () => {
        if (file && deckId) {
            try {
                const importedWords = await importWordsFromExcel(deckId, file);
                setWords([...words, ...importedWords]);
            } catch (err) {
                setError(err instanceof Error ? err.message : "Something went wrong");
            }

        }
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        try {
            if (deck && user) {
                await updateDeck(user.id, deck.id, form);
                setDeck({ ...deck, ...form });
            }
        } catch (err) {
            setError("Something went wrong");
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm({ ...form, [name]: value });
    };

    return (
        <>
            <div>
                <input type="file" onChange={handleFileChange} />
                <button onClick={handleUpload}>Upload</button>
            </div>
            <div>
                <h2>Deck {deck?.name}</h2>
                <h4>Deck {deck?.description}</h4>
                <button type="submit">Delete</button>
                <form onSubmit={handleSubmit} className="deck-form">
                    <input placeholder="name"
                        name="name"
                        value={form.name ?? ""} onChange={handleChange} />
                    <input placeholder="description"
                        name="description"
                        value={form.description ?? ""} onChange={handleChange} />
                    <button type="submit">Update</button>
                    {error && <p>{error}</p>}
                </form>
                {words.map((word) => (
                    <p key={word.id}>{word.term}     {word.reading}     {word.meaning}</p>
                ))}
            </div>
        </>
    );
}
