import React from "react";
import { Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { Home } from "./components/Home";
import { Catalog } from "./components/Catalog";
import { Inventory } from "./components/Inventory";
import { Users } from "./components/Users";
import { Store } from "./components/Store";
import AuthorizeRoute from "./components/api-authorization/AuthorizeRoute";
import ApiAuthorizationRoutes from "./components/api-authorization/ApiAuthorizationRoutes";
import { ApplicationPaths } from "./components/Constants";

export default function App() {
  return (
    <Routes>
      {/* Layout is a route element */}
      <Route path="/" element={<Layout />}>
        {/* Child routes render into <Outlet /> inside Layout */}
        <Route index element={<Home />} />
        <Route
          path={ApplicationPaths.CatalogPath}
          element={
            <AuthorizeRoute
              path={ApplicationPaths.CatalogPath}
              element={<Catalog />}
            />
          }
        />
        <Route
          path={ApplicationPaths.InventoryPath}
          element={
            <AuthorizeRoute
              path={ApplicationPaths.InventoryPath}
              element={<Inventory />}
            />
          }
        />
        <Route
          path={ApplicationPaths.UsersPath}
          element={
            <AuthorizeRoute
              path={ApplicationPaths.UsersPath}
              element={<Users />}
            />
          }
        />
        <Route
          path={ApplicationPaths.StorePath}
          element={
            <AuthorizeRoute
              path={ApplicationPaths.StorePath}
              element={<Store />}
            />
          }
        />
        <Route path="/authentication/*" element={<ApiAuthorizationRoutes />} />
      </Route>
    </Routes>
  );
}
