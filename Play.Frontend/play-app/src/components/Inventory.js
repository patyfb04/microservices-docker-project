import React, { useEffect, useState } from "react";
import { Col, Container, Row, Table, Button } from "react-bootstrap";
import { useLocation, useNavigate } from "react-router-dom";
import authService from "./api-authorization/AuthorizeService";

export function Inventory() {
  const location = useLocation();
  const navigate = useNavigate();
  const userFromUsersPage = location.state?.user;

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadedSuccess, setLoadedSuccess] = useState(false);

  useEffect(() => {
    const populateItems = async () => {
      let userId = "";

      if (userFromUsersPage) {
        userId = userFromUsersPage.id;
      } else {
        const user = await authService.getUser();
        userId = user.sub;
      }

      try {
        const token = await authService.getAccessToken();
        const response = await fetch(
          `${window.INVENTORY_ITEMS_API_URL}?userId=${userId}`,
          {
            headers: !token ? {} : { Authorization: `Bearer ${token}` },
          }
        );
        const returnedItems = await response.json();
        setItems(returnedItems);
        setLoadedSuccess(true);
      } catch (err) {
        console.error(err);
        setItems([]);
        setLoadedSuccess(false);
      } finally {
        setLoading(false);
      }
    };

    populateItems();
  }, [userFromUsersPage]);

  const renderItemsTable = (items) => (
    <Container style={{ paddingTop: "10px", paddingLeft: "0px" }}>
      <Row>
        <Col>
          <Table striped>
            <thead className="thead-dark">
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Quantity</th>
              </tr>
            </thead>
            <tbody>
              {!items || items.length <= 0 ? (
                <tr>
                  <td colSpan="6" align="center">
                    <b>No Items yet</b>
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.catalogItemId}>
                    <td>{item.name}</td>
                    <td>{item.description}</td>
                    <td>{item.quantity}</td>
                  </tr>
                ))
              )}
            </tbody>
          </Table>
        </Col>
      </Row>
      <Row>
        <Col>
          {userFromUsersPage && (
            <Button color="primary" onClick={() => navigate(-1)}>
              Back to Users
            </Button>
          )}
        </Col>
      </Row>
    </Container>
  );

  return (
    <div>
      <h1 id="tabelLabel">
        {userFromUsersPage ? userFromUsersPage.username : "My"} Inventory
      </h1>
      {loading ? (
        <p>
          <em>Loading...</em>
        </p>
      ) : loadedSuccess ? (
        renderItemsTable(items)
      ) : (
        <p>Could not load items</p>
      )}
    </div>
  );
}
