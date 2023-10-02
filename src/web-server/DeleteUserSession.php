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

if (!isset($_POST["SessionToken"]) || !isset($_POST["Username"]))
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

// Deleteing the associated user session.

$opResponse = DeleteUserSession($connectionDb, $userId);
echo($opResponse);

?>