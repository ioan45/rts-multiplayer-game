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

if (!isset($_POST["SessionToken"]) || !isset($_POST["Username"]) || !isset($_POST["ReturnServer"])
    || ($_POST["ReturnServer"] !== "Yes" && $_POST["ReturnServer"] !== "No"))
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
$returnServer = ($_POST["ReturnServer"] == "Yes" ? true : false);



// Database query (using prepared statements) to check if the user is registered as being in match.

$inMatchQuery = null;
if ($returnServer)
    $inMatchQuery = "SELECT asv.ip, asv.port, asv.password
                     FROM user us, user_in_match uim, allocated_server asv
                     WHERE us.username = ? and 
                           us.user_id = uim.user_id and
                           uim.server_id = asv.server_id";
else
    $inMatchQuery = "SELECT 1
                     FROM user us, user_in_match uim
                     WHERE us.username = ? and us.user_id = uim.user_id";

$statement = $connectionDb->prepare($inMatchQuery);
if ($statement === false)
{
    echo("0\tQuery preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('s', $username);
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
$inMatchResult = $statement->get_result();
if ($inMatchResult === false)
{
    echo("0\tQuery result retrieval failed. " . $statement->error);
    die();
}

if ($inMatchResult->num_rows == 0)
{
    // The user is not in match.
    echo('2');
    exit();
}
if ($returnServer)
{
    // The user is in match.
    $resultRow = $inMatchResult->fetch_array(MYSQLI_NUM);
    echo("1\t" . $resultRow[0] . "\t" . $resultRow[1] . "\t" . $resultRow[2]);
}
else
    // The user is in match.
    echo("1");

?>