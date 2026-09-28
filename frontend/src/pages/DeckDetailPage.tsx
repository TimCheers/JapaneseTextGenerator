import { useParams } from "react-router-dom";
import { useState, useEffect, useContext } from "react"
import { importWordsFromExcel, getWordsForDeck, createWord, type Word } from "../api/words"
import { AuthContext } from "../context/AuthContext"
import { getDeckById, updateDeck, type Deck, type CreateDeckRequest, deleteDeck } from "../api/decks"
import { type CreateWordRequest } from "../api/words"
import { useNavigate } from "react-router-dom";



export function DeckDetailPage() {
    const navigate = useNavigate();
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
                const createdWord = await createWord(deckId, wordForm);
                setWords([...words, createdWord]);
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
            }
        } catch (err) {
            setError("Something went wrong");
        }
    }
    const handleChangeWord = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setWordForm({ ...wordForm, [name]: value });
    };

    return (
        <>
            <div>
                <input type="file" onChange={handleFileChange} />
                <button onClick={handleUploadWords}>Upload</button>
            </div>
            <div>
                <h2>Deck {deck?.name}</h2>
                <h4>Deck {deck?.description}</h4>
                <button type="button" onClick={handleDeleteDeck}>Delete deck</button>
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
                    <button type="submit">Create word</button>
                    {error && <p>{error}</p>}
                </form>
                {words.map((word) => (
                    <p key={word.id}>{word.term}     {word.reading}     {word.meaning}</p>
                ))}
            </div>
        </>
    );
}
