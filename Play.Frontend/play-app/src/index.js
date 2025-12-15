import "bootstrap/dist/css/bootstrap.min.css";
import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import reportWebVitals from "./reportWebVitals";

const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href");
const root = ReactDOM.createRoot(document.getElementById("root"));

root.render(
  <BrowserRouter basename={baseUrl}>
    <App />
  </BrowserRouter>
);

reportWebVitals();
