import { Link } from "react-router-dom";
import "../styles/Header.css";

export function Header() {
    return (
        <nav className="header">
            <Link to="/">Main page</Link>
            <Link to="/profile">Profile</Link>
            <Link to="/decks">Decks</Link>
            <Link to="/register">Registration</Link>
            <Link to="/login">Login</Link>
        </nav>

    );
}