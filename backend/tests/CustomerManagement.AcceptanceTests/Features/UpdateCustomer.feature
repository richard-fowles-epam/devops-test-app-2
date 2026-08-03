Feature: Update customer
    As an API consumer
    I want to update customers via PUT /customers/{id}
    So that existing customer records can be modified

Scenario: Update a customer with valid details
    Given a customer with the following details
        | FirstName | LastName | Email           |
        | Ada       | Lovelace | ada@example.com |
    And the customer has been created via POST /customers
    When the customer is updated with the following details
        | FirstName | LastName | Email            |
        | Alan      | Turing   | alan@example.com |
    Then the response status is 200
    And the updated customer should match the new details

Scenario: Update a customer with a non-existent ID
    When a customer update is submitted for a non-existent ID
        | FirstName | LastName | Email            |
        | Alan      | Turing   | alan@example.com |
    Then the response status is 404

Scenario: Reject an update with a missing required field
    Given a customer with the following details
        | FirstName | LastName | Email           |
        | Ada       | Lovelace | ada@example.com |
    And the customer has been created via POST /customers
    When the customer is updated with the following details
        | FirstName | LastName | Email            |
        |           | Turing   | alan@example.com |
    Then the response status is 400
