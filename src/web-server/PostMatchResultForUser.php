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

if (!isset($_POST["Username"]) || !isset($_POST["Trophies"]) || !isset($_POST["Gold"]) || !isset($_POST["Unit_id"]))
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$username = $connectionDb->escape_string($_POST['Username']);
$trophies = intval($_POST["Trophies"]);
$gold = intval($_POST["Gold"]);
$unitId = ($_POST["Unit_id"] == "NO_UNIT" ? false : intval($_POST["Unit_id"]));

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

// Database query (using prepared statements) to update the player's trophies and gold.

$updateDataQuery = "UPDATE player_data
                    SET gold = gold + (?), trophies = GREATEST(0, trophies + (?))
                    WHERE user_id = ?";
$statement = $connectionDb->prepare($updateDataQuery);
if ($statement === false)
{
    echo("0\tUpdatePlayerData: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('iii', $gold, $trophies, $userId);
if (!$opSucceeded)
{
    echo("0\tUpdatePlayerData: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tUpdatePlayerData: Statement execution failed. " . $statement->error);
    die();
}

if ($unitId !== false)
{
    // Database query (using prepared statements) to check if the user already has the combat unit.

    $unitExistsQuery = "SELECT 1 FROM owned_combat_unit WHERE user_id = ? and combat_unit_id = ?";
    $statement = $connectionDb->prepare($unitExistsQuery);
    if ($statement === false)
    {
        echo("0\tCheckNewUnitOwned: Query preparing failed. " . $connectionDb->error);
        die();
    }
    $opSucceeded = $statement->bind_param('ii', $userId, $unitId);
    if (!$opSucceeded)
    {
        echo("0\tCheckNewUnitOwned: Parameters bounding failed. " . $statement->error);
        die();
    }
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
    {
        echo("0\tCheckNewUnitOwned: Statement execution failed. " . $statement->error);
        die();
    }
    $unitExistsResult = $statement->get_result();
    if ($unitExistsResult === false)
    {
        echo("0\tCheckNewUnitOwned: Query result retrieval failed. " . $statement->error);
        die();
    }
    
    if ($unitExistsResult->num_rows == 0)
    {
        // Database query (using prepared statements) to insert the new gained combat unit.

        $insertUnitQuery = "INSERT INTO owned_combat_unit VALUES(?, ?, 1, null)";
        $statement = $connectionDb->prepare($insertUnitQuery);
        if ($statement === false)
        {
            echo("0\tInsertGainedUnit: Query preparing failed. " . $connectionDb->error);
            die();
        }
        $opSucceeded = $statement->bind_param('ii', $userId, $unitId);
        if (!$opSucceeded)
        {
            echo("0\tInsertGainedUnit: Parameters bounding failed. " . $statement->error);
            die();
        }
        $opSucceeded = $statement->execute();
        if (!$opSucceeded)
        {
            echo("0\InsertGainedUnit: Statement execution failed. " . $statement->error);
            die();
        }   
    }
}

echo("1");

?>
