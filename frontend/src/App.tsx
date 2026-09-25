import { Routes, Route } from "react-router-dom";
import { DecksPage } from "./pages/DecksPage"
import { HomePage } from "./pages/HomePage"
import { LoginPage } from "./pages/LoginPage"
import { ProfilePage } from "./pages/ProfilePage"
import { RegisterPage } from "./pages/RegisterPage"
import { Layout } from "./components/Layout"

function App() {


  return (
    <div>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/decks" element={<DecksPage />} />
        </Route>
      </Routes>
    </div>
  );
}

export default App;