import { useEffect } from "react";
import authService from "./AuthorizeService";

export default function SilentRenew() {
  useEffect(() => {
    async function run() {
      try {
        await authService.ensureUserManagerInitialized();
        await authService.userManager.signinSilentCallback(
          window.location.href
        );
      } catch (e) {
        console.error("Silent renew callback error:", e);
      }
    }
    run();
  }, []);

  // This page is never actually seen by the user; it runs inside an iframe.
  return <div>Silent renew</div>;
}
