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

if (!isset($_POST["SessionToken"]))
{
    echo("0\tOne or more input arguments are not provided.");
    die();
}
$sessionToken = $connectionDb->escape_string($_POST['SessionToken']);

// Database query (using prepared statements) to retrieve data about the session with the given token (if exists).

$sessionDataQuery = "SELECT us.user_id, us.username, us.player_name, uls.session_start_date, pd.gold, pd.trophies
                     FROM user us, user_login_session uls, player_data pd
                     WHERE us.user_id = uls.user_id and 
                           us.user_id = pd.user_id and 
                           uls.session_token = ?";
$statement = $connectionDb->prepare($sessionDataQuery);
if ($statement === false)
{
    echo("0\tGettingSessionData: Query preparing failed. " . $connectionDb->error);
    die();
}
$opSucceeded = $statement->bind_param('s', $sessionToken);
if (!$opSucceeded)
{
    echo("0\tGettingSessionData: Parameters bounding failed. " . $statement->error);
    die();
}
$opSucceeded = $statement->execute();
if (!$opSucceeded)
{
    echo("0\tGettingSessionData: Statement execution failed. " . $statement->error);
    die();
}
$sessionDataResult = $statement->get_result();
if ($sessionDataResult === false)
{
    echo("0\tGettingSessionData: Query result retrieval failed. " . $statement->error);
    die();
}
if ($sessionDataResult->num_rows == 0)
{
    echo("0\tNo user has a session with the given token.");
    exit();
}
$sessionData = $sessionDataResult->fetch_array(MYSQLI_NUM);
if ($sessionData === false)
{
    echo("0\tGettingSessionData: Couldn't fetch_array() from the result object.");
    die();
}

// At this point, there is a registered session with the given token.
// Testing if the session is still active.

$sessionStartDate = new DateTime($sessionData[3]);
$presentDate = new DateTime();
$daysBetween = ($presentDate->diff($sessionStartDate))->days;
$maxSessionActiveDays = 1;
if ($daysBetween >= $maxSessionActiveDays)
{
    // Session is expired.
    // A query is made to delete the expired session from database.
    $opResponse = DeleteUserSession($connectionDb, $sessionData[0]);
    if ($opResponse[0] == '1')
        echo("0\tSession expired.");
    else
        echo($opResponse);
}
else
{
    // Session is still active.
    // Getting the owned combat units encodings.

    $ownedUnitsEncodings = null;
    $opResponse = GetOwnedCombatUnits($connectionDb, $sessionData[1]);
    if (is_string($opResponse))
    {
        echo("0\t" . $opResponse);
        die();
    }
    else
        $ownedUnitsEncodings = $opResponse;

    echo("1\t". $sessionData[1] . "\t" . $sessionData[2] . "\t" . $sessionData[4] . "\t" . $sessionData[5]);
    echo("\t" . $ownedUnitsEncodings[0] . "\t" . $ownedUnitsEncodings[1] . "\t" . $ownedUnitsEncodings[2]);
}

?>