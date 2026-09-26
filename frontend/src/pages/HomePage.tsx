import { useState, useEffect, useContext } from "react"
import { AuthContext } from "../context/AuthContext"
import type { GeneratedText } from "../api/generatedTexts"
import { getGeneratedTexts } from "../api/generatedTexts"
import { TextCard } from "../components/TextCard"
import { createGenerationRequest } from "../api/generationRequests"



export function HomePage() {
    const { user } = useContext(AuthContext);
    const [texts, setTexts] = useState<GeneratedText[]>([]);

    const [isGenerating, setIsGenerating] = useState<boolean>(false);

    const handleScroll = async (e: React.UIEvent<HTMLDivElement>) => {
        const { scrollTop, scrollHeight, clientHeight } = e.currentTarget;
        const reachedBottom = scrollTop + clientHeight >= scrollHeight - 1;
        if (reachedBottom) {
            if (!user || isGenerating) return;
            setIsGenerating(true);

            try {
                await createGenerationRequest(user.id);
                const result = await getGeneratedTexts(user.id);
                setTexts(result);
            } catch (err) {

            } finally {
                setIsGenerating(false);
            }
        }

    };


    useEffect(() => {
        if (user) {
            getGeneratedTexts(user.id).then((result) => setTexts(result));
        }
    }, [user]);




    return (
        <div>
            <h1>Home page</h1>
            <div className="texts-container">
                <div className="texts-list" onScroll={handleScroll}>
                    {[...texts].reverse().map((text) => (<TextCard key={text.id} text={text} />))}
                </div>
                {isGenerating && (
                    <div className="loading-indicator">
                        <div className="spinner"></div>
                    </div>
                )}
            </div>
        </div>
    );
}