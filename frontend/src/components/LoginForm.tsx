import { useState } from "react";
import type { LoginRequest } from "../api/auth";
import { loginUser } from "../api/auth";

export function LoginForm() {
    const [form, setForm] = useState<LoginRequest>({
        email: "",
        password: "",
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setForm({ ...form, [name]: value });
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            const result = await loginUser(form);
            localStorage.setItem("authToken", result.authToken);
            setSuccess(true);
        } catch (err) {
            setError("Что-то пошло не так");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            {!success &&
                <form onSubmit={handleSubmit}>
                    <input placeholder="email" name="email" value={form.email} onChange={handleChange} />
                    <input placeholder="password" name="password" type="password" value={form.password} onChange={handleChange} />
                    <button type="submit">Логин</button>
                    {error && <p>{error}</p>}
                </form>
            }
            {success && <p>Авторизация прошла успешно!</p>}
        </div>

    );
}