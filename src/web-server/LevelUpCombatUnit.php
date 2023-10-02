<?php

require_once "DbConnectionData.php";
require_once "CommonQueries.php";

// Connect to database

$connectionDb = new mysqli($serverDb, $usernameDb, $passwordDb, $nameDb);
if ($connectionDb->connect_errno)
{
    echo("0\tDB connection error: " + $connectionDb->connect_error);
    die();
}
$connectionDb->set_charset("utf8");

// Validate input

if (!isset($_POST["SessionToken"]) || !isset($_POST["Username"]) || !isset($_POST["UnitId"]) 
    || !isset($_POST["ToLevel"]) || !isset($_POST["GoldUsed"]))
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$username = $connectionDb->escape_string($_POST['Username']);
if (ValidateSessionToken($connectionDb, $connectionDb->escape_string($_POST["SessionToken"]), $username) !== true)
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$unitId = intval($_POST["UnitId"]);
$newUnitLevel = intval($_POST["ToLevel"]);
$goldUsed = intval($_POST["GoldUsed"]);

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

// Database query to update the unit level (for specified user).

$updateLevelQuery = "UPDATE owned_combat_unit
                     SET unit_level = ?
                     WHERE user_id = ? and combat_unit_id = ?";
$statement = $connectionDb->prepare($updateLevelQuery);
if ($statement === false)
{
    echo("0\tUpdateUnitLevel: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('iii', $newUnitLevel, $userId, $unitId);
if (!$opSucceeded)
{
    echo("0\tUpdateUnitLevel: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tUpdateUnitLevel: Statement execution failed. " . $statement->error);
    die();
}

// Database query to update the user's gold amount.

$updateGoldQuery = "UPDATE player_data
                     SET gold = gold - ? 
                     WHERE user_id = ?";
$statement = $connectionDb->prepare($updateGoldQuery);
if ($statement === false)
{
    echo("0\tUpdateUserGold: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('ii', $goldUsed, $userId);
if (!$opSucceeded)
{
    echo("0\tUpdateUserGold: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tUpdateUserGold: Statement execution failed. " . $statement->error);
    die();
}

echo("1");

?>
