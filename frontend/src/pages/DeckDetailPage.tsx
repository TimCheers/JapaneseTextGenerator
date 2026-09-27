import { useParams } from "react-router-dom";
import { useState, useEffect, useContext } from "react"
import type { Word } from "../api/words"
import { getWordsForDeck } from "../api/words"



export function DeckDetailPage() {
    const { deckId } = useParams<{ deckId: string }>();
    const [words, setWords] = useState<Word[]>([]);


    useEffect(() => {
        if (deckId) {
            getWordsForDeck(deckId).then((result) => setWords(result));
        }
    }, [deckId]);

    return (
        <div>
            <h2>Deck {deckId}</h2>
            {words.map((word) => (
                <p key={word.id}>{word.term}     {word.reading}     {word.meaning}</p>
            ))}
        </div>
    );
}
