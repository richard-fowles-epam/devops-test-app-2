Feature: Delete customer
    As an API consumer
    I want to delete a customer by their ID via DELETE /customers/{id}
    So that I can remove a customer record from the system

Scenario: Deleting an existing customer returns 204 No Content
    Given a customer with the following details
        | FirstName | LastName | Email              |
        | Grace     | Hopper   | grace@example.com  |
    And the customer has been created via POST /customers
    When the customer is deleted by their ID
    Then the response status is 204

Scenario: Deleting a non-existent customer returns 404 Not Found
    When a customer is deleted with a non-existent ID
    Then the response status is 404

Scenario: After deleting a customer, a subsequent GET returns 404 Not Found
    Given a customer with the following details
        | FirstName | LastName | Email               |
        | Alan      | Turing   | alan@example.com    |
    And the customer has been created via POST /customers
    When the customer is deleted by their ID
    And the deleted customer is requested by their ID
    Then the response status is 404
