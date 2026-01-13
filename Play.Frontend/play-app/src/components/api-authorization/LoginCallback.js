import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import authService from "./AuthorizeService";

export default function LoginCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    async function run() {
      try {
        const result = await authService.completeSignIn(window.location.href);
        navigate(result.state?.returnUrl || "/");
      } catch (err) {
        console.error("Login callback error:", err);
      }
    }
    run();
  }, [navigate]);

  return <div>Processing login callback…</div>;
}
