import { AuthForm } from "../components/auth-form";
import styles from "./login.module.css";

export default function LoginPage() {
  return (
    <div className={styles.login}>
      <AuthForm mode="login" />
    </div>
  );
}
