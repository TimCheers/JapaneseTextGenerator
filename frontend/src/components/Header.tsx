import { Link } from "react-router-dom";
import { useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import "../styles/Header.css";

export function Header() {
    const { token, logout } = useContext(AuthContext);

    return (
        <nav className="header">
            <Link to="/">Main page</Link>
            {token ? (
                <>
                    <Link to="/profile">Profile</Link>
                    <Link to="/decks">Decks</Link>
                    <button className="header-logout" onClick={logout}>Logout</button>
                </>
            ) : (
                <>
                    <Link to="/register">Registration</Link>
                    <Link to="/login">Login</Link>
                </>
            )}
        </nav>

    );
}