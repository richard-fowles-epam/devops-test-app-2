using System.ComponentModel.DataAnnotations;

namespace CustomerManagement.Api.Models;

/// <summary>
/// The request body for <c>PUT /products/{id}</c>.
/// <c>name</c> and <c>price</c> are required. <c>description</c> is optional.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>The product name. Required.</summary>
    /// <example>Keyboard</example>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>The product description. Optional.</summary>
    /// <example>Mechanical keyboard with tactile switches</example>
    public string? Description { get; set; }

    /// <summary>The product price. Required and must be greater than zero.</summary>
    /// <example>129.99</example>
    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal? Price { get; set; }
}
