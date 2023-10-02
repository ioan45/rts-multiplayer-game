<?php

require_once "DbConnectionData.php";
require_once "GenerateToken.php";
require_once "CommonQueries.php";

function IsInputValid() : bool
{
    if (!ctype_alnum($_POST["Username"]))
        return false;
    $length = strlen($_POST["Username"]);
    if ($length < 3)
        return false;
    if ($length > 25)
        return false;
    if (strlen($_POST["Password"]) != mb_strlen($_POST["Password"], "UTF-8"))  // password must contain only one byte chars
        return false;
    $length = strlen($_POST["Password"]);
    if ($length < 10)
        return false;
    if ($length > 40)
        return false;
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

if (!isset($_POST["Username"]) || !isset($_POST["Password"]))
{
    echo("0\tOne or more input arguments are not provided.");
    die();
}
if (!IsInputValid())
{
    echo("2");
    exit();
}

$username = $connectionDb->escape_string($_POST['Username']);
$password = $connectionDb->escape_string($_POST['Password']);

// Database query (using prepared statements) to check if user exists

$userExistsQuery = "SELECT us.user_id, us.password_hash, us.player_name, pd.gold, pd.trophies 
                    FROM user us, player_data pd
                    WHERE us.user_id = pd.user_id and 
                          us.username = ?";
$statement = $connectionDb->prepare($userExistsQuery);
if ($statement === false)
{
    echo("0\tUserExists: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('s', $username);
if (!$opSucceeded)
{
    echo("0\tUserExists: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tUserExists: Statement execution failed. " . $statement->error);
    die();
}
$userExistsResult = $statement->get_result();
if ($userExistsResult === false)
{
    echo("0\tQuery result retrieval failed. " . $statement->error);
    die();
}
if ($userExistsResult->num_rows == 0)
{
    // Username doesn't exist.
    echo("2");
    exit();
}
$resultRow = $userExistsResult->fetch_array(MYSQLI_NUM);
$userId = $resultRow[0];
$passwordHash = $resultRow[1];
$playerName = $resultRow[2];
$gold = $resultRow[3];
$trophies = $resultRow[4];

if (!password_verify($password, $passwordHash))
{
    // Password is invalid for the user.
    echo("2");
    exit();
}

// At this point, the given credentials are correct for logging in.
// Removing old session record (if exists) from database.

$opResponse = DeleteUserSession($connectionDb, $userId);
if ($opResponse[0] == '0')
{
    echo($opResponse);
    die();
}

// Creating new session and adding it to the database.

$newSessionToken = GenerateToken(64);
$sessionStartDate = date("Y-m-d H:i:s");
$newSessionQuery = "INSERT INTO user_login_session VALUES(?, ?, ?)";
$statement = $connectionDb->prepare($newSessionQuery);
if ($statement === false)
{
    echo("0\tCreateSession: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('iss', $userId, $newSessionToken, $sessionStartDate);
if (!$opSucceeded)
{
    echo("0\tCreateSession: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tCreateSession: Statement execution failed. " . $statement->error);
    die();
}

// Getting the owned combat units encodings.

$ownedUnitsEncodings = null;
$opResponse = GetOwnedCombatUnits($connectionDb, $username);
if (is_string($opResponse))
{
    echo("0\t" . $opResponse);
    die();
}
else
    $ownedUnitsEncodings = $opResponse;

echo("1\t". $newSessionToken . "\t" . $playerName . "\t" . $gold . "\t" . $trophies);
echo("\t" . $ownedUnitsEncodings[0] . "\t" . $ownedUnitsEncodings[1] . "\t" . $ownedUnitsEncodings[2]);

?>