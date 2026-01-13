import React from "react";
import { Routes, Route } from "react-router-dom";
import { Login } from "./Login";
import { Logout } from "./Logout";
import { LoginActions, LogoutActions } from "./ApiAuthorizationConstants";
import LoginCallback from "./LoginCallback";
import SilentRenew from "./SilentRenew";

export default function ApiAuthorizationRoutes() {
  return (
    <Routes>
      <Route
        path={LoginActions.Login}
        element={<Login action={LoginActions.Login} />}
      />
      <Route
        path={LoginActions.LoginFailed}
        element={<Login action={LoginActions.LoginFailed} />}
      />
      <Route path={LoginActions.LoginCallback} element={<LoginCallback />} />
      <Route path="/authentication/silent-renew" element={<SilentRenew />} />
      <Route
        path={LoginActions.Profile}
        element={<Login action={LoginActions.Profile} />}
      />
      <Route
        path={LoginActions.Register}
        element={<Login action={LoginActions.Register} />}
      />
      <Route
        path={LogoutActions.Logout}
        element={<Logout action={LogoutActions.Logout} />}
      />
      <Route
        path={LogoutActions.LogoutCallback}
        element={<Logout action={LogoutActions.LogoutCallback} />}
      />
      <Route
        path={LogoutActions.LoggedOut}
        element={<Logout action={LogoutActions.LoggedOut} />}
      />
    </Routes>
  );
}
