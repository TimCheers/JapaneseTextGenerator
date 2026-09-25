import { Outlet } from "react-router-dom";
import { Footer } from "./Footer";
import { Header } from "./Header";
import "../styles/Layout.css";


export function Layout() {
    return (
        <div className="layout-root">
            <Header />
            <div className="layout-content"><Outlet /></div>
            <Footer />
        </div>
    );
}