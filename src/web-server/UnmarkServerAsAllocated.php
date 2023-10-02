<?php

require_once "DbConnectionData.php";

// Connect to database

$connectionDb = new mysqli($serverDb, $usernameDb, $passwordDb, $nameDb);
if ($connectionDb->connect_errno)
{
    echo("0\tDB connection error: " + $connectionDb->connect_error);
    die();
}
$connectionDb->set_charset("utf8");

// Validate input

if (!isset($_POST["Id"]) || !is_numeric($_POST['Id']))
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$serverId = intval($_POST['Id']);

// Firstly, any remained user association with the server is deleted.

$deleteAssocQuery = "DELETE FROM user_in_match WHERE server_id = ?";
$statement = $connectionDb->prepare($deleteAssocQuery);
if ($statement === false)
{
    echo("0\tDeleteUserAssoc: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('i', $serverId);
if (!$opSucceeded)
{
    echo("0\tDeleteUserAssoc: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tDeleteUserAssoc: Statement execution failed. " . $statement->error);
    die();
}

// Database query to delete the server from the allocated_server table.

$unmarkQuery = "DELETE FROM allocated_server WHERE server_id = ?";
$statement = $connectionDb->prepare($unmarkQuery);
if ($statement === false)
{
    echo("0\tDeleteAllocatedServer: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('i', $serverId);
if (!$opSucceeded)
{
    echo("0\tDeleteAllocatedServer: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tDeleteAllocatedServer: Statement execution failed. " . $statement->error);
    die();
}

echo('1');

?>