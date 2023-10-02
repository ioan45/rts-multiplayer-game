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

if (!isset($_POST["Username"]) || !isset($_POST["Ip"]) || !isset($_POST["Port"])
    || !filter_var($_POST["Ip"], FILTER_VALIDATE_IP)
    || !is_numeric($_POST['Port']))
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$username = $connectionDb->escape_string($_POST['Username']);
$serverIp = $connectionDb->escape_string($_POST['Ip']);
$serverPort = intval($_POST['Port']);

// Database query (using prepared statements) to get the associated user id.

$userId = null;
$opResponse = GetUserId($connectionDb, $username);
if (!is_numeric($opResponse))
{
    echo("0\t" . $opResponse);
    die();
}
else
    $userId = $opResponse;

// Database query (using prepared statements) to get the associated server id.

$serverId = null;
$opResponse = GetServerId($connectionDb, $serverIp, $serverPort);
if (!is_numeric($opResponse))
{
    echo("0\t" . $opResponse);
    die();
}
else
    $serverId = $opResponse;

// Database query (using prepared statements) to mark the user as being in match.

$markUserQuery = "INSERT INTO user_in_match VALUES(?, ?)";
$statement = $connectionDb->prepare($markUserQuery);
if ($statement === false)
{
    echo("0\tMarkUser: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('ii', $userId, $serverId);
if (!$opSucceeded)
{
    echo("0\tMarkUser: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tMarkUser: Statement execution failed. " . $statement->error);
    die();
}

echo("1");

?>