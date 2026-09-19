import { AuthForm } from "../components/auth-form";
import authStyles from "../login/login.module.css";

export default function RegisterPage() {
  return (
    <div className={authStyles.login}>
      <AuthForm mode="register" />
    </div>
  );
}
