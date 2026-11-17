
// F# Practical Assignment: Library Management System

open System
open Microsoft.Data.Sqlite


// 1. DOMAIN MODEL: F# TYPES AND RECORDS


// Book status using Discriminated Union
type BookStatus =
    | Available
    | Borrowed

// Book record
type Book = {
    Id: int
    Title: string
    Author: string
    Year: int
    Status: BookStatus
}

// Database connection
let connectionString = "Data Source=library.db"

// Convert book status into string for database
let statusToString status =
    match status with
    | Available -> "Available"
    | Borrowed -> "Borrowed"
 
// 2. DATABASE INITIALIZATION


let initializeDatabase () = async {
    use connection = new SqliteConnection(connectionString)

    do! connection.OpenAsync() |> Async.AwaitTask

    use command = connection.CreateCommand()

    command.CommandText <-
        """
        CREATE TABLE IF NOT EXISTS Books (
            Id INTEGER PRIMARY KEY,
            Title TEXT NOT NULL,
            Author TEXT NOT NULL,
            Year INTEGER NOT NULL,
            Status TEXT NOT NULL
        )
        """

    let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

    printfn "Database initialized successfully!"
}


// 3. CREATE: ADD A BOOK


let addBook (book: Book) = async {
    use connection = new SqliteConnection(connectionString)

    do! connection.OpenAsync() |> Async.AwaitTask

    use command = connection.CreateCommand()

    command.CommandText <-
        """
        INSERT INTO Books (Id, Title, Author, Year, Status)
        VALUES (@id, @title, @author, @year, @status)
        """

    command.Parameters.AddWithValue("@id", book.Id) |> ignore
    command.Parameters.AddWithValue("@title", book.Title) |> ignore
    command.Parameters.AddWithValue("@author", book.Author) |> ignore
    command.Parameters.AddWithValue("@year", book.Year) |> ignore
    command.Parameters.AddWithValue("@status", statusToString book.Status) |> ignore

    let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

    printfn "Book added: %s" book.Title
}


// 4. READ: GET ALL BOOKS


let getAllBooks () = async {
    use connection = new SqliteConnection(connectionString)

    do! connection.OpenAsync() |> Async.AwaitTask

    use command = connection.CreateCommand()

    command.CommandText <- "SELECT * FROM Books ORDER BY Id"

    use reader = command.ExecuteReader()

    let mutable books = []
    let mutable reading = true

    while reading do
        let! hasRow = reader.ReadAsync() |> Async.AwaitTask

        if hasRow then
            let status =
                match reader.GetString(4) with
                | "Available" -> Available
                | _ -> Borrowed

            let book = {
                Id = reader.GetInt32(0)
                Title = reader.GetString(1)
                Author = reader.GetString(2)
                Year = reader.GetInt32(3)
                Status = status
            }

            books <- book :: books
        else
            reading <- false

    return List.rev books
}


// 5. UPDATE: UPDATE BOOK DETAILS


let updateBook (book: Book) = async {
    use connection = new SqliteConnection(connectionString)

    do! connection.OpenAsync() |> Async.AwaitTask

    use command = connection.CreateCommand()

    command.CommandText <-
        """
        UPDATE Books
        SET Title = @title,
            Author = @author,
            Year = @year,
            Status = @status
        WHERE Id = @id
        """

    command.Parameters.AddWithValue("@id", book.Id) |> ignore
    command.Parameters.AddWithValue("@title", book.Title) |> ignore
    command.Parameters.AddWithValue("@author", book.Author) |> ignore
    command.Parameters.AddWithValue("@year", book.Year) |> ignore
    command.Parameters.AddWithValue("@status", statusToString book.Status) |> ignore

    let! rows = command.ExecuteNonQueryAsync() |> Async.AwaitTask

    if rows > 0 then
        printfn "Book updated successfully!"
    else
        printfn "Book not found!"
}


// 6. DELETE: REMOVE A BOOK


let deleteBook id = async {
    use connection = new SqliteConnection(connectionString)

    do! connection.OpenAsync() |> Async.AwaitTask

    use command = connection.CreateCommand()

    command.CommandText <- "DELETE FROM Books WHERE Id = @id"

    command.Parameters.AddWithValue("@id", id) |> ignore

    let! rows = command.ExecuteNonQueryAsync() |> Async.AwaitTask

    if rows > 0 then
        printfn "Book deleted successfully!"
    else
        printfn "Book not found!"
}

// 7. HIGHER-ORDER FUNCTIONS

// List.map: Extract book titles
let getBookTitles books =
    List.map (fun book -> book.Title) books

// List.filter: Select available books
let getAvailableBooks books =
    List.filter (fun book ->
        match book.Status with
        | Available -> true
        | Borrowed -> false
    ) books

// 8. PATTERN MATCHING


let displayBookStatus status =
    match status with
    | Available -> "Available"
    | Borrowed -> "Borrowed"


// 9. DISPLAY BOOK RECORDS

let displayBooks books =
    printfn "\n----- Library Books -----"

    books
    |> List.iter (fun book ->
        printfn "ID: %d | Title: %s | Author: %s | Year: %d | Status: %s"
            book.Id
            book.Title
            book.Author
            book.Year
            (displayBookStatus book.Status)
    )

// 10. MAIN PROGRAM


[<EntryPoint>]
let main argv =

    async {

        // Initialize database
        do! initializeDatabase()

        // Remove previous demo records so output
        // remains consistent when the program is rerun.
        do! deleteBook 1
        do! deleteBook 2
        do! deleteBook 3

        // Sample book records
        let book1 = {
            Id = 1
            Title = "Clean Code"
            Author = "Robert Martin"
            Year = 2008
            Status = Available
        }

        let book2 = {
            Id = 2
            Title = "The Pragmatic Programmer"
            Author = "Andrew Hunt"
            Year = 1999
            Status = Borrowed
        }

        let book3 = {
            Id = 3
            Title = "Atomic Habits"
            Author = "James Clear"
            Year = 2018
            Status = Available
        }

        // Add books
        printfn "\nAdding Books:"

        do! addBook book1
        do! addBook book2
        do! addBook book3

        // Read and display all books
        let! books = getAllBooks()

        displayBooks books

        // Higher-order function: List.map
        printfn "\n----- List.map: Book Titles -----"

        let titles = getBookTitles books

        titles |> List.iter (printfn "%s")

        // Higher-order function: List.filter
        printfn "\n----- List.filter: Available Books -----"

        let availableBooks = getAvailableBooks books

        displayBooks availableBooks

        // Pattern matching
        printfn "\n----- Pattern Matching -----"

        books
        |> List.iter (fun book ->
            match book.Status with
            | Available ->
                printfn "%s is available." book.Title
            | Borrowed ->
                printfn "%s is borrowed." book.Title
        )

        // Update book
        printfn "\n----- Updating Book -----"

        let updatedBook = {
            book2 with
                Title = "The Pragmatic Programmer - Updated"
                Status = Available
        }

        do! updateBook updatedBook

        // Delete book
        printfn "\n----- Deleting Book -----"

        do! deleteBook 3

        // Display final database records
        let! finalBooks = getAllBooks()

        printfn "\n----- Final Library Records -----"

        displayBooks finalBooks

        printfn "\nProgram completed successfully!"

    }
    |> Async.RunSynchronously

    0
