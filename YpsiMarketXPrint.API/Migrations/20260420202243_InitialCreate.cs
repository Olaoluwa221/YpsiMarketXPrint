using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YpsiMarketXPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "ProductTypes",
                    columns: table => new
                    {
                        ProductTypeId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        TypeName = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ProductTypes", x => x.ProductTypeId);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Users",
                    columns: table => new
                    {
                        UserId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        FirstName = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        LastName = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Email = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        PasswordHash = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        MarketingOptIn = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        UserType = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Users", x => x.UserId);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Products",
                    columns: table => new
                    {
                        ProductId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        ProductName = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ProductTypeId = table.Column<int>(type: "int", nullable: false),
                        RequiresArtwork = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Products", x => x.ProductId);
                        table.ForeignKey(
                            name: "FK_Products_ProductTypes_ProductTypeId",
                            column: x => x.ProductTypeId,
                            principalTable: "ProductTypes",
                            principalColumn: "ProductTypeId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Carts",
                    columns: table => new
                    {
                        CartId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        UserId = table.Column<int>(type: "int", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Carts", x => x.CartId);
                        table.ForeignKey(
                            name: "FK_Carts_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "UserId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Orders",
                    columns: table => new
                    {
                        OrderId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        UserId = table.Column<int>(type: "int", nullable: true),
                        GuestEmail = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        DateOrdered = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        DeliveryMethod = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        OrderStatus = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Orders", x => x.OrderId);
                        table.ForeignKey(
                            name: "FK_Orders_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "UserId"
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "PasswordResetTokens",
                    columns: table => new
                    {
                        Id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        UserId = table.Column<int>(type: "int", nullable: false),
                        Token = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        Used = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                        table.ForeignKey(
                            name: "FK_PasswordResetTokens_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "UserId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Pictures",
                    columns: table => new
                    {
                        PictureId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        UploaderId = table.Column<int>(type: "int", nullable: false),
                        Link = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Pictures", x => x.PictureId);
                        table.ForeignKey(
                            name: "FK_Pictures_Users_UploaderId",
                            column: x => x.UploaderId,
                            principalTable: "Users",
                            principalColumn: "UserId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "ProductVariants",
                    columns: table => new
                    {
                        VariantId = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        ProductId = table.Column<int>(type: "int", nullable: false),
                        Size = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ProductVariants", x => x.VariantId);
                        table.ForeignKey(
                            name: "FK_ProductVariants_Products_ProductId",
                            column: x => x.ProductId,
                            principalTable: "Products",
                            principalColumn: "ProductId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "ProductPictures",
                    columns: table => new
                    {
                        ProductId = table.Column<int>(type: "int", nullable: false),
                        PictureId = table.Column<int>(type: "int", nullable: false),
                        IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey(
                            "PK_ProductPictures",
                            x => new { x.ProductId, x.PictureId }
                        );
                        table.ForeignKey(
                            name: "FK_ProductPictures_Pictures_PictureId",
                            column: x => x.PictureId,
                            principalTable: "Pictures",
                            principalColumn: "PictureId",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_ProductPictures_Products_ProductId",
                            column: x => x.ProductId,
                            principalTable: "Products",
                            principalColumn: "ProductId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "CartItems",
                    columns: table => new
                    {
                        CartId = table.Column<int>(type: "int", nullable: false),
                        VariantId = table.Column<int>(type: "int", nullable: false),
                        Quantity = table.Column<int>(type: "int", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_CartItems", x => new { x.CartId, x.VariantId });
                        table.ForeignKey(
                            name: "FK_CartItems_Carts_CartId",
                            column: x => x.CartId,
                            principalTable: "Carts",
                            principalColumn: "CartId",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_CartItems_ProductVariants_VariantId",
                            column: x => x.VariantId,
                            principalTable: "ProductVariants",
                            principalColumn: "VariantId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "OrderItems",
                    columns: table => new
                    {
                        OrderId = table.Column<int>(type: "int", nullable: false),
                        VariantId = table.Column<int>(type: "int", nullable: false),
                        Quantity = table.Column<int>(type: "int", nullable: false),
                        UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                        ArtworkUrl = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_OrderItems", x => new { x.OrderId, x.VariantId });
                        table.ForeignKey(
                            name: "FK_OrderItems_Orders_OrderId",
                            column: x => x.OrderId,
                            principalTable: "Orders",
                            principalColumn: "OrderId",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_OrderItems_ProductVariants_VariantId",
                            column: x => x.VariantId,
                            principalTable: "ProductVariants",
                            principalColumn: "VariantId",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_VariantId",
                table: "CartItems",
                column: "VariantId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems",
                column: "VariantId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_UploaderId",
                table: "Pictures",
                column: "UploaderId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProductPictures_PictureId",
                table: "ProductPictures",
                column: "PictureId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CartItems");

            migrationBuilder.DropTable(name: "OrderItems");

            migrationBuilder.DropTable(name: "PasswordResetTokens");

            migrationBuilder.DropTable(name: "ProductPictures");

            migrationBuilder.DropTable(name: "Carts");

            migrationBuilder.DropTable(name: "Orders");

            migrationBuilder.DropTable(name: "ProductVariants");

            migrationBuilder.DropTable(name: "Pictures");

            migrationBuilder.DropTable(name: "Products");

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "ProductTypes");
        }
    }
}
