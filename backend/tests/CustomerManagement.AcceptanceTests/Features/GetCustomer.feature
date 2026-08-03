Feature: Get customer
    As an API consumer
    I want to retrieve a customer by their ID via GET /customers/{id}
    So that I can look up an individual customer record

Scenario: Retrieve an existing customer by ID
    Given a customer with the following details
        | FirstName | LastName | Email           |
        | Ada       | Lovelace | ada@example.com |
    And the customer has been created via POST /customers
    When the customer is requested by their ID
    Then the get customer response status is 200
    And the retrieved customer should match the created customer details

Scenario: Request a customer with a non-existent ID
    When a customer is requested with a non-existent ID
    Then the get customer response status is 404
