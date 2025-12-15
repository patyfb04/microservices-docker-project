// AuthorizeRoute.js
import React from "react";
import { Navigate } from "react-router-dom";
import {
  AuthorizationPaths,
  QueryParameterNames,
} from "./ApiAuthorizationConstants";
import authService from "./AuthorizeService";

export default function AuthorizeRoute({ element, path }) {
  const [ready, setReady] = React.useState(false);
  const [authenticated, setAuthenticated] = React.useState(false);

  React.useEffect(() => {
    const sub = authService.subscribe(() => populateAuth());
    populateAuth();
    return () => authService.unsubscribe(sub);
  }, []);

  async function populateAuth() {
    const isAuth = await authService.isAuthenticated();
    setReady(true);
    setAuthenticated(isAuth);
  }

  if (!ready) return <div />;

  const link = document.createElement("a");
  link.href = path;
  const returnUrl = `${link.protocol}//${link.host}${link.pathname}${link.search}${link.hash}`;
  const redirectUrl = `${AuthorizationPaths.Login}?${
    QueryParameterNames.ReturnUrl
  }=${encodeURI(returnUrl)}`;

  return authenticated ? element : <Navigate to={redirectUrl} replace />;
}
