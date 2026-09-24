import { RegisterForm } from "./components/RegisterForm.tsx";
import { LoginForm } from "./components/LoginForm.tsx";
import { useState } from "react";

function App() {
  const [isRegisterMode, setIsRegisterMode] = useState(true);
  return (
    <div>
            {isRegisterMode ? <RegisterForm /> : <LoginForm />}
            <button onClick={() => setIsRegisterMode(!isRegisterMode)}>Переключить</button>
        </div>
  );
}

export default App;