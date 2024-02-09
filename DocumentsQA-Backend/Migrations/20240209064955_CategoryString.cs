using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentsQA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class CategoryString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

			// Map category enum ints to string
			migrationBuilder.Sql(
				@"
					update [Questions]
					set Category = 
						(case q.Category 
							when 0 then 'general' 
							when 1 then 'collateral' 
							when 2 then 'litigation' 
							else 'general' 
						end)
					from [Questions] as q
				");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "Questions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

			// Map category string back to enum int
			migrationBuilder.Sql(
				@"
					update [Questions]
					set Category = 
						(case q.Category 
							when 'general' then 0 
							when 'collateral' then 1 
							when 'litigation' then 2 
							else 0 
						end)
					from [Questions] as q
				");
		}
    }
}
