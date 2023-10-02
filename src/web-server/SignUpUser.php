<?php

require_once "DbConnectionData.php";
require_once "CommonQueries.php";

function ValidateInput() : bool|string
{
    if (!ctype_alnum($_POST["Username"]))
        return "Username contains invalid characters";
    $length = strlen($_POST["Username"]);
    if ($length < 3)
        return "Username is too short";
    if ($length > 25)
        return "Username is too long";
    if (strlen($_POST["Password"]) != mb_strlen($_POST["Password"], "UTF-8"))  // password must contain only one byte chars
        return "Password contains invalid characters";
    $length = strlen($_POST["Password"]);
    if ($length < 10)
        return "Password is too short";
    if ($length > 40)
        return "Password is too long";
    if (strlen($_POST["Player_name"]) != mb_strlen($_POST["Player_name"], "UTF-8"))  // player name must contain only one byte chars
        return "Player name contains invalid characters";
    if (trim($_POST["Player_name"]) != $_POST["Player_name"])
        return "Player name starts or ends with white space characters";
    $length = strlen($_POST["Player_name"]);
    if ($length < 3)
        return "Player name is too short";
    if ($length > 25)
        return "Player name is too long";
    if (!filter_var($_POST["Email"], FILTER_VALIDATE_EMAIL))
        return "The given email address is invalid.";
    return true;
}

// Connect to database

$connectionDb = new mysqli($serverDb, $usernameDb, $passwordDb, $nameDb);
if ($connectionDb->connect_errno)
{
    echo("0\tDB connection error: " + $connectionDb->connect_error);
    die();
}
$connectionDb->set_charset("utf8");

// Validate input

if (!isset($_POST["Username"]) || !isset($_POST["Password"]) || !isset($_POST["Player_name"]) || !isset($_POST["Email"]) 
    || !isset($_POST["Start_deck"]) || !isset($_POST["Initial_gold"]) || !isset($_POST["Initial_trophies"]))
{
    echo("0\tOne or more input arguments are not provided.");
    die();
}
$validationResult = ValidateInput();
if ($validationResult !== true)
{
    echo("2\t$validationResult");
    exit();
}
$unitIdsList = explode("&", $_POST["Start_deck"]);
for ($i = 0; $i < count($unitIdsList); ++$i)
{
    $unitIdsList[$i] = intval($unitIdsList[$i]);
    if ($unitIdsList[$i] == 0)
    {
        echo("0\tOne or more input arguments are not provided or are invalid.");
        die();
    }
}
$username = $connectionDb->escape_string($_POST['Username']);
$passwordHash = password_hash($_POST['Password'], PASSWORD_DEFAULT);  // uses the bcrypt algorithm
$playerName = $connectionDb->escape_string($_POST['Player_name']);
$email = $connectionDb->escape_string($_POST['Email']);
$initialGold = intval($_POST["Initial_gold"]);
$initialTrophies = intval($_POST["Initial_trophies"]);
$registrationDate = date("Y-m-d H:i:s");

// Checking if the username already exists.

$usernameExistsQuery = "SELECT 1 FROM user WHERE username = ?";
$statement = $connectionDb->prepare($usernameExistsQuery);
if ($statement === false)
{
    echo("0\tCheckUsername: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('s', $username);
if (!$opSucceeded)
{
    echo("0\tCheckUsername: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tCheckUsername: Statement execution failed. " . $statement->error);
    die();
}
$usernameExistsResult = $statement->get_result();
if ($usernameExistsResult === false)
{
    echo("0\tCheckUsername: Query result retrieval failed. " . $statement->error);
    die();
}
if ($usernameExistsResult->num_rows != 0)
{
    echo("2\tA player with the chosen username already exists");
    exit();
}

// Since the username is not taken, register the new user.

$insertUserQuery = "INSERT INTO user VALUES(NULL, ?, ?, ?, ?, ?)";
$statement = $connectionDb->prepare($insertUserQuery);
if ($statement === false)
{
    echo("0\tCreateUser: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('sssss', $username, $passwordHash, $email, $playerName, $registrationDate);
if (!$opSucceeded)
{
    echo("0\tCreateUser: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tCreateUser: Statement execution failed. " . $statement->error);
    die();
}

// Database query (using prepared statements) to get the user id.

$userId = null;
$opResponse = GetUserId($connectionDb, $username);
if (!is_numeric($opResponse))
{
    echo("0\t" . $opResponse);
    die();
}
else
    $userId = $opResponse;

// Inserting the initial player data.

$insertPlayerDataQuery = "INSERT INTO player_data VALUES(?, ?, ?)";
$statement = $connectionDb->prepare($insertPlayerDataQuery);
if ($statement === false)
{
    echo("0\tCreatePlayerData: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('iii', $userId, $initialGold, $initialTrophies);
if (!$opSucceeded)
{
    echo("0\tCreatePlayerData: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tCreatePlayerData: Statement execution failed. " . $statement->error);
    die();
}

// Inserting the starting deck.

$insertDeckQuery = "INSERT INTO owned_combat_unit VALUES";
for ($i = 0; $i < count($unitIdsList); ++$i)
{
    $insertDeckQuery .= " (" . $userId . ", " . $unitIdsList[$i] . ", 1, " . ($i + 1) . ")";
    if ($i < count($unitIdsList) - 1)
        $insertDeckQuery .= ",";
}
$insertDeckQR = $connectionDb->query($insertDeckQuery);
if ($insertDeckQR === false)
{
    echo("0\tCreateDeck: Insert deck failed. " + $connectionDb->error);
    die();
}

echo('1');

?>