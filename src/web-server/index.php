<?php

date_default_timezone_set('Europe/Bucharest');

$requestPath = trim($_SERVER['REQUEST_URI'], '/');
$requestPath = explode('?', $requestPath)[0];

switch ($requestPath)
{
    case "check_if_session_exists":
        require_once "CheckIfSessionExists.php";
        break;
    case "check_if_user_in_match":
        require_once "CheckIfUserInMatch.php";
        break;
    case "delete_user_session";
        require_once "DeleteUserSession.php";
        break;
    case "get_server_password";
        require_once "GetServerPassword.php";
        break;
    case "mark_server_as_allocated";
        require_once "MarkServerAsAllocated.php";
        break;
    case "mark_user_as_in_match";
        require_once "MarkUserAsInMatch.php";
        break;
    case "server_connection_testing";
        require_once "ServerConnectionTesting.php";
        break;
    case "sign_in_user";
        require_once "SignInUser.php";
        break;
    case "sign_up_user";
        require_once "SignUpUser.php";
        break;
    case "unmark_server_as_allocated";
        require_once "UnmarkServerAsAllocated.php";
        break;
    case "unmark_users_as_in_match":
        require_once "UnmarkUsersAsInMatch.php";
        break;
    case "save_deck":
        require_once "SaveDeck.php";
        break;
    case "level_up_unit":
        require_once "LevelUpCombatUnit.php";
        break;
    case "post_match_result":
        require_once "PostMatchResultForUser.php";
        break;
    default:
        http_response_code(404);
        exit ('Resource not found.');
}

?>