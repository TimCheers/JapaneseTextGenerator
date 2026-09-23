import { useEffect, useState } from "react";
import { getDecks, type Deck } from "./api/decks";
import { TextFeed } from "./components/TextFeed";
import { mockGeneratedTexts } from "./mocks/generatedTexts";

const TEMP_USER_ID = "01532162-7df0-4911-a479-9a3b9986f29f";

function App() {
  return <TextFeed texts={mockGeneratedTexts} />;
}

export default App;

// function App() {
//   const [decks, setDecks] = useState<Deck[]>([]);
//   const [isLoading, setIsLoading] = useState(true);
//   const [error, setError] = useState<string | null>(null);

//   useEffect(() => {
//     async function loadDecks() {
//       try {
//         const data = await getDecks(TEMP_USER_ID);
//         setDecks(data);
//       } catch (err) {
//         setError(err instanceof Error ? err.message : "Unknown error");
//       } finally {
//         setIsLoading(false);
//       }
//     }

//     loadDecks();
//   }, []);

//   if (isLoading) return <p>Loading...</p>;
//   if (error) return <p>Error: {error}</p>;

//   return (
//     <ul>
//       {decks.map((deck) => (
//         <li key={deck.id}>{deck.name}</li>
//       ))}
//     </ul>
//   );
// }

// export default App;