const urlParams = new URLSearchParams(window.location.search);
const username = getOption('username', 'KommadantKlink');
const is_right = getOptionExist('right');

function getOptionExist(key)
{
    return urlParams.get(key) != null;
}

function getOption(key, def = null) {
    let res = urlParams.get(key);
    return res == null ? def : res;
}

function getOperators(callback, include_session = false)
{
    if (!include_session) $.get(getSiegeURL('/operators.php'), { username: username }, callback, 'json');
    else                  $.get(getSiegeURL('/operators.php'), { session: true, username: username }, callback, 'json');
}

function getOperator(callback, operator)
{
     $.get(getSiegeURL('/operators.php'), { username: username, operator: operator }, callback, 'json');
}

function getSiegeURL(endpoint)
{
    let local_api = "https://d.lu.je/siege";
    let remote_api = "../";
    return (location.protocol == "file:" ? local_api : remote_api) + endpoint;
}
