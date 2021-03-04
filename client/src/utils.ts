export const postDataAsJson = async (
  url: string,
  data: object = {}
): Promise<any> => {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  });
  return await response.json();
};

export const sendDelete = async (url: string): Promise<Response> => {
  const response = await fetch(url, {
    method: "DELETE",
  });
  return response;
};
