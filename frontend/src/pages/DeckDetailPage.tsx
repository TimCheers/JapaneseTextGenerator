import { useParams } from "react-router-dom";
import { useState, useEffect } from "react"
import type { Word } from "../api/words"
import { importWordsFromExcel } from "../api/words"
import { getWordsForDeck } from "../api/words"



export function DeckDetailPage() {
    const { deckId } = useParams<{ deckId: string }>();
    const [words, setWords] = useState<Word[]>([]);
    const [file, setFile] = useState<File | null>(null);
    const [error, setError] = useState("");


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

    return (
        <>
            <div>
                <input type="file" onChange={handleFileChange} />
                <button onClick={handleUpload}>Upload</button>
            </div>
            <div>
                <h2>Deck {deckId}</h2>
                {words.map((word) => (
                    <p key={word.id}>{word.term}     {word.reading}     {word.meaning}</p>
                ))}
                {error && <p className="generation-error">{error}</p>}
            </div>
        </>
    );
}
