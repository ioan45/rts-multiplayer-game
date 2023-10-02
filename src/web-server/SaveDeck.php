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

if (!isset($_POST["SessionToken"]) || !isset($_POST["Username"]) || !isset($_POST["Deck"]))
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
$unitIdsList = explode("&", $_POST["Deck"]);
for ($i = 0; $i < count($unitIdsList); ++$i)
{
    $unitIdsList[$i] = intval($unitIdsList[$i]);
    if ($unitIdsList[$i] == 0)
    {
        echo("0\tOne or more input arguments are not provided or are invalid.");
        die();
    }
}
$unitIdsQueryValue = $connectionDb->escape_string(str_replace("&", ",", $_POST["Deck"]));
$unitIdsQueryValue = "(" . $unitIdsQueryValue . ")";

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

// Remove old deck configuration.

$removeDeckQuery = "UPDATE owned_combat_unit
                    SET position_in_deck = null
                    WHERE user_id = ?";
$statement = $connectionDb->prepare($removeDeckQuery);
if ($statement === false)
{
    echo("0\tRemoveOldDeck: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('i', $userId);
if (!$opSucceeded)
{
    echo("0\tRemoveOldDeck: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tRemoveOldDeck: Statement execution failed. " . $statement->error);
    die();
}

// Update with the new deck configuration.

$updateDeckQuery = "UPDATE owned_combat_unit
                    SET position_in_deck = CASE";
for ($i = 0; $i < count($unitIdsList); ++$i)
    $updateDeckQuery .= " WHEN combat_unit_id = " . $unitIdsList[$i] . " THEN " . ($i + 1);
$updateDeckQuery .= " ELSE combat_unit_id END";
$updateDeckQuery .= " WHERE user_id = ? and combat_unit_id IN $unitIdsQueryValue";

$statement = $connectionDb->prepare($updateDeckQuery);
if ($statement === false)
{
    echo("0\tRemoveOldDeck: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('i', $userId);
if (!$opSucceeded)
{
    echo("0\tRemoveOldDeck: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tRemoveOldDeck: Statement execution failed. " . $statement->error);
    die();
}
echo("1");

?>
