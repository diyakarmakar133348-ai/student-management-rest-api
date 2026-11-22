
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQL Server connection
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? @"Server=.\SQLEXPRESS;Database=StudentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";

// Register database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create database automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Home endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "Welcome to Student Management API",
    documentation = "/swagger"
}));

// Student API group
var studentsApi = app.MapGroup("/api/students")
                     .WithTags("Students");

// GET: Get all students
studentsApi.MapGet("/", async (AppDbContext db) =>
{
    var students = await db.Students
        .AsNoTracking()
        .OrderBy(s => s.Id)
        .ToListAsync();

    return Results.Ok(students);
});

// GET: Get student by ID
studentsApi.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    var student = await db.Students.FindAsync(id);

    if (student == null)
    {
        return Results.NotFound(new
        {
            message = $"Student with ID {id} not found."
        });
    }

    return Results.Ok(student);
});

// POST: Add new student
studentsApi.MapPost("/", async (Student student, AppDbContext db) =>
{
    var error = ValidateStudent(student);

    if (error != null)
        return Results.BadRequest(new { message = error });

    student.Id = 0;

    db.Students.Add(student);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/students/{student.Id}", student);
});

// PUT: Update student
studentsApi.MapPut("/{id:int}",
    async (int id, Student updatedStudent, AppDbContext db) =>
{
    var error = ValidateStudent(updatedStudent);

    if (error != null)
        return Results.BadRequest(new { message = error });

    var student = await db.Students.FindAsync(id);

    if (student == null)
    {
        return Results.NotFound(new
        {
            message = $"Student with ID {id} not found."
        });
    }

    student.Name = updatedStudent.Name.Trim();
    student.Email = updatedStudent.Email.Trim();
    student.Age = updatedStudent.Age;
    student.Course = updatedStudent.Course.Trim();

    await db.SaveChangesAsync();

    return Results.Ok(student);
});

// DELETE: Delete student
studentsApi.MapDelete("/{id:int}",
    async (int id, AppDbContext db) =>
{
    var student = await db.Students.FindAsync(id);

    if (student == null)
    {
        return Results.NotFound(new
        {
            message = $"Student with ID {id} not found."
        });
    }

    db.Students.Remove(student);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = $"Student with ID {id} deleted successfully."
    });
});

// Run application
app.Run();

// Student validation
static string? ValidateStudent(Student student)
{
    if (string.IsNullOrWhiteSpace(student.Name))
        return "Name is required.";

    if (string.IsNullOrWhiteSpace(student.Email) ||
        !student.Email.Contains('@') ||
        !student.Email.Contains('.'))
        return "A valid email address is required.";

    if (student.Age < 1 || student.Age > 120)
        return "Age must be between 1 and 120.";

    if (string.IsNullOrWhiteSpace(student.Course))
        return "Course is required.";

    return null;
}

// Student Model
public class Student
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Course { get; set; } = string.Empty;
}

// Database Context
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
}
