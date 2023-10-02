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

if (!isset($_POST["SessionToken"]) || !isset($_POST["Ip"]) || !isset($_POST["Port"])
    || !filter_var($_POST["Ip"], FILTER_VALIDATE_IP)
    || !is_numeric($_POST['Port']))
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
$serverIp = $connectionDb->escape_string($_POST['Ip']);
$serverPort = intval($_POST['Port']);


// Database query (using prepared statements) for password

$getPasswordQuery = "SELECT password 
                     FROM allocated_server
                     WHERE ip = ? and port = ?";
$statement = $connectionDb->prepare($getPasswordQuery);
if ($statement === false)
{
    echo("0\tQuery preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('si', $serverIp, $serverPort);
if (!$opSucceeded)
{
    echo("0\tParameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tStatement execution failed. " . $statement->error);
    die();
}
$getPasswordResult = $statement->get_result();
if ($getPasswordResult === false)
{
    echo("0\tQuery result retrieval failed. " . $statement->error);
    die();
}
if ($getPasswordResult->num_rows == 0)
{
    echo("0\tCorresponding server was not found.");
    die();
}
$serverPassword = $getPasswordResult->fetch_array(MYSQLI_NUM)[0];
echo("1\t" . $serverPassword);

?>