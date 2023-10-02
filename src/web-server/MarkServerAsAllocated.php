<?php

require_once "DbConnectionData.php";
require_once "GenerateToken.php";

// Connect to database

$connectionDb = new mysqli($serverDb, $usernameDb, $passwordDb, $nameDb);
if ($connectionDb->connect_errno)
{
    echo("0\tDB connection error: " + $connectionDb->connect_error);
    die();
}
$connectionDb->set_charset("utf8");

// Validate input

if (!isset($_POST["Id"]) || !isset($_POST["Ip"]) || !isset($_POST["Port"])
    || !is_numeric($_POST['Id'])
    || !filter_var($_POST["Ip"], FILTER_VALIDATE_IP)
    || !is_numeric($_POST['Port']))
{
    echo("0\tOne or more input arguments are not provided or are invalid.");
    die();
}
$serverId = intval($_POST['Id']);
$serverIp = $connectionDb->escape_string($_POST['Ip']);
$serverPort = intval($_POST['Port']);

// Generating server password.

$serverPassword = $connectionDb->escape_string(GenerateToken(64));

// Normally, at this point, there shouldn't be a record with the same server ID in the table.
// But, if there is because of an error, it will be rewritten by the current data.

// Checking if there is already a record with the current server ID.

$checkServerIdQuery = "SELECT 1 FROM allocated_server WHERE server_id = ?";
$statement = $connectionDb->prepare($checkServerIdQuery);
if ($statement === false)
{
    echo("CheckServerId: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('i', $serverId);
if (!$opSucceeded)
{
    echo("CheckServerId: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("CheckServerId: Statement execution failed. " . $statement->error);
    die();
}
$serverIdResult = $statement->get_result();
if ($serverIdResult === false)
{
    echo("CheckServerId: Query result retrieval failed. " . $statement->error);
    die();
}
$recordExists = false;
if ($serverIdResult->num_rows != 0)
    $recordExists = true;

if ($recordExists)
{
    // Updating the found record.
    
    $markQuery = "UPDATE allocated_server
                  SET ip = ?, port = ?, password = ?
                  WHERE server_id = ?";
    $statement = $connectionDb->prepare($markQuery);
    if ($statement === false)
    {
        echo("0\tUpdateRecord: Query preparing failed. " . $connectionDb->error);
        die();
    }
    $opSucceeded = $statement->bind_param('sisi', $serverIp, $serverPort, $serverPassword, $serverId);
    if (!$opSucceeded)
    {
        echo("0\tUpdateRecord: Parameters bounding failed. " . $statement->error);
        die();
    }
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
    {
        echo("0\tUpdateRecord: Statement execution failed. " . $statement->error);
        die();
    }
}
else
{
    // Marking the server as allocated by adding new record.

    $markQuery = "INSERT INTO allocated_server VALUES(?, ?, ?, ?)";
    $statement = $connectionDb->prepare($markQuery);
    if ($statement === false)
    {
        echo("0\tAddNewRecord: Query preparing failed. " . $connectionDb->error);
        die();
    }
    $opSucceeded = $statement->bind_param('isis', $serverId, $serverIp, $serverPort, $serverPassword);
    if (!$opSucceeded)
    {
        echo("0\tAddNewRecord: Parameters bounding failed. " . $statement->error);
        die();
    }
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
    {
        echo("0\tAddNewRecord: Statement execution failed. " . $statement->error);
        die();
    }
}

echo("1\t" . $serverPassword);

?>