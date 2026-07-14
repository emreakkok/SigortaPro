import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/app/App";
import { AppProviders } from "@/app/providers";
import "@/styles/globals.css";

const rootElement = document.getElementById("root");
if (rootElement === null) {
  throw new Error("Kök element (#root) bulunamadı — index.html bozulmuş olabilir.");
}

createRoot(rootElement).render(
  <StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </StrictMode>,
);
