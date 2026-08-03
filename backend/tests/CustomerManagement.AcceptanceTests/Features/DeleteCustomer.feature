Feature: Delete customer
    As an API consumer
    I want to delete customers via DELETE /customers/{id}
    So that obsolete customer records can be removed

Scenario: Delete an existing customer by ID
    Given a customer with the following details
        | FirstName | LastName | Email           |
        | Ada       | Lovelace | ada@example.com |
    And the customer has been created via POST /customers
    When the customer is deleted by their ID
    Then the response status is 204
    When the deleted customer is requested by their ID
    Then the get customer response status is 404

Scenario: Delete a customer with a non-existent ID
    When a customer is deleted with a non-existent ID
    Then the response status is 404
