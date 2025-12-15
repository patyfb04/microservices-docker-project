import React from "react";
import { Container } from "react-bootstrap";
import { NavMenu } from "./NavMenu";
import { Footer } from "./Footer";
import { Outlet } from "react-router-dom";

export function Layout() {
  return (
    <div>
      <NavMenu />
      <Container>
        <Outlet /> {/* child routes render here */}
      </Container>
      <Footer />
    </div>
  );
}
