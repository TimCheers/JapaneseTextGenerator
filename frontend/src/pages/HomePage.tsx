import { useState, useEffect, useContext } from "react"
import { AuthContext } from "../context/AuthContext"
import type { GeneratedText } from "../api/generatedTexts"
import { getGeneratedTexts } from "../api/generatedTexts"
import { TextCard } from "../components/TextCard"



export function HomePage() {
    const { user } = useContext(AuthContext);
    const [texts, setTexts] = useState<GeneratedText[]>([]);

    useEffect(() => {
        if (user) {
            getGeneratedTexts(user.id).then((result) => setTexts(result));
        }
    }, [user]);




    return (
        <div>
            <h1>Home page</h1>
            <div className="texts-list">
                {texts.map((text) => (<TextCard key={text.id} text={text} />))}
            </div>
        </div>
    );
}