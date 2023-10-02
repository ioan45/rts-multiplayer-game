<?php

require_once "DbConnectionData.php";

function ValidateSessionToken(mysqli $connectionDb, string $token, ?string $username = null) : bool|string
{
    // If username is provided, it checks if the given token is associated with the username,
    // else it checks just if the token has not expired.

    $maxSessionActiveDays = 7;

    $validateQuery = null;
    if (is_null($username))
        $validateQuery = "SELECT 1 
                          FROM user_login_session 
                          WHERE session_token = ? and 
                                session_start_date > now() - interval $maxSessionActiveDays day";
    else
        $validateQuery = "SELECT 1 
                          FROM user_login_session uls, user us 
                          WHERE us.username = ? and
                                us.user_id = uls.user_id and
                                uls.session_token = ?";
    $statement = $connectionDb->prepare($validateQuery);
    if ($statement === false)
        return "ValidateSessionToken: Query preparing failed. " . $connectionDb->error;
    $opSucceeded = null;
    if (is_null($username))
        $opSucceeded = $statement->bind_param('s', $token);
    else
        $opSucceeded = $statement->bind_param('ss', $username, $token);
    if (!$opSucceeded)
        return "ValidateSessionToken: Parameters bounding failed. " . $statement->error;
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
        return "ValidateSessionToken: Statement execution failed. " . $statement->error;
    $queryResult = $statement->get_result();
    if ($queryResult === false)
        return "ValidateSessionToken: Query result retrieval failed. " . $statement->error;
    if ($queryResult->num_rows == 0)
        return false;
    return true;
}

function DeleteUserSession(mysqli $connectionDb, string|int $userId) : string
{
    $deleteSessionQuery = "DELETE FROM user_login_session WHERE user_id = ?";
    $statement = $connectionDb->prepare($deleteSessionQuery);
    if ($statement === false)
        return "0\tDeleteExpiredSession: Query preparing failed. " . $connectionDb->error;
    $opSucceeded = $statement->bind_param('i', $userId);
    if (!$opSucceeded)
        return "0\tDeleteExpiredSession: Parameters bounding failed. " . $statement->error;
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
        return "0\tDeleteExpiredSession: Statement execution failed. " . $statement->error;
    return "1";
}

function GetUserId(mysqli $connectionDb, string $username) : string
{
    $userIdQuery = "SELECT user_id FROM user WHERE username = ?";
    $statement = $connectionDb->prepare($userIdQuery);
    if ($statement === false)
        return "GetUserId: Query preparing failed. " . $connectionDb->error;
    $opSucceeded = $statement->bind_param('s', $username);
    if (!$opSucceeded)
        return "GetUserId: Parameters bounding failed. " . $statement->error;
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
        return "GetUserId: Statement execution failed. " . $statement->error;
    $userIdResult = $statement->get_result();
    if ($userIdResult === false)
        return "GetUserId: Query result retrieval failed. " . $statement->error;
    if ($userIdResult->num_rows == 0)
        return "Corresponding user was not found.";
    return $userIdResult->fetch_array(MYSQLI_NUM)[0];
}

function GetServerId(mysqli $connectionDb, string $ip, string|int $port) : string
{
    $getServerIdQuery = "SELECT server_id FROM allocated_server WHERE ip = ? and port = ?";
    $statement = $connectionDb->prepare($getServerIdQuery);
    if ($statement === false)
        return "GetServerId: Query preparing failed. " . $connectionDb->error;
    $opSucceeded = $statement->bind_param('si', $ip, $port);
    if (!$opSucceeded)
        return "GetServerId: Parameters bounding failed. " . $statement->error;
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
        return "GetServerId: Statement execution failed. " . $statement->error;
    $serverIdResult = $statement->get_result();
    if ($serverIdResult === false)
        return "GetServerId: Query result retrieval failed. " . $statement->error;
    if ($serverIdResult->num_rows == 0)
        return "Corresponding server was not found.";
    return $serverIdResult->fetch_array(MYSQLI_NUM)[0];
}

function GetOwnedCombatUnits(mysqli $connectionDb, string $username) : array|string
{
    // Returns an array with 3 elements: 
    // - owned units IDs encoding
    // - owned units levels encoding (in the same units order as the units IDs encoding)
    // - deck units IDs encoding (in deck position order)
    // Encoding format: value1&value2&value3&...&valueN

    $getUnitsQuery = "SELECT combat_unit_id, unit_level, position_in_deck
                      FROM owned_combat_unit ocu, user us
                      WHERE ocu.user_id = us.user_id and us.username = ?";
    $statement = $connectionDb->prepare($getUnitsQuery);
    if ($statement === false)
        return "GetOwnedUnits: Query preparing failed. " . $connectionDb->error;
    $opSucceeded = $statement->bind_param('s', $username);
    if (!$opSucceeded)
        return "GetOwnedUnits: Parameters bounding failed. " . $statement->error;
    $opSucceeded = $statement->execute();
    if (!$opSucceeded)
        return "GetOwnedUnits: Statement execution failed. " . $statement->error;
    $getUnitsResult = $statement->get_result();
    if ($getUnitsResult === false)
        return "GetOwnedUnits: Query result retrieval failed. " . $statement->error;
    if ($getUnitsResult->num_rows == 0)
        return "GetOwnedUnits: Didn't find any unit.";
    $resultRows = $getUnitsResult->fetch_all(MYSQLI_ASSOC);

    $ownedUnitsIds = array();
    $ownedUnitsLevels = array();
    $deckUnitsIds = array();
    foreach ($resultRows as $row) 
    {
        $ownedUnitsIds[] = $row["combat_unit_id"];
        $ownedUnitsLevels[] = $row["unit_level"];
        if (!is_null($row["position_in_deck"]))
            $deckUnitsIds[$row["position_in_deck"]] = $row["combat_unit_id"];  // The key(index) corresponds to the position in deck.
    }                                                                          // (This doesn't ensure key ordering in array)
    
    $resultArray = array();
    $resultArray[] = implode("&", $ownedUnitsIds);
    $resultArray[] = implode("&", $ownedUnitsLevels);
    $deckEncoding = "";
    $deckSize = count($deckUnitsIds);
    for ($i = 1; $i <= $deckSize - 1; ++$i)
        $deckEncoding .= ($deckUnitsIds[$i] . "&");
    $deckEncoding .= $deckUnitsIds[$deckSize];
    $resultArray[] = $deckEncoding;
    return $resultArray; 
}

?>