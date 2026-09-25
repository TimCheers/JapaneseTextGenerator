import { useState, useContext } from "react";
import type { RegisterRequest } from "../api/auth";
import { registerUser } from "../api/auth";
import { AuthContext } from "../context/AuthContext";
import "../styles/AuthForm.css";

export function RegisterForm() {
    const [form, setForm] = useState<RegisterRequest>({
        displayName: "",
        email: "",
        password: "",
        nativeLanguage: ""
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const { login } = useContext(AuthContext);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm({ ...form, [name]: value });
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            const result = await registerUser(form);
            login(result.authToken);
            setSuccess(true);
        } catch (err) {
            setError("Something went wrong");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            {!success &&
                <form className="auth-form" onSubmit={handleSubmit}>
                    <input placeholder="displayName" name="displayName" value={form.displayName} onChange={handleChange} />
                    <input placeholder="email" name="email" value={form.email} onChange={handleChange} />
                    <input placeholder="password" name="password" type="password" value={form.password} onChange={handleChange} />
                    <input placeholder="nativeLanguage" name="nativeLanguage" value={form.nativeLanguage ?? ""} onChange={handleChange} />
                    <button type="submit">Sign Up</button>
                    {error && <p>{error}</p>}
                </form>
            }
            {success && <p>Registration was successful!</p>}
        </div>

    );
}