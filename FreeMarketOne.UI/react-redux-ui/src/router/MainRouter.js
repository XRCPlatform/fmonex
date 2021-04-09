// React
import React, { useEffect, Suspense } from "react";
import { Helmet } from "react-helmet";

// Router
import { Route, Redirect, Switch, useHistory } from "react-router-dom";
import { publicRoutes, privateRoutes } from "Router/Routes";

// Layouts
import MainLayout from "Layouts/MainLayout";

// Loaders
import Spinner from "Layouts/loaders/Spinner";

const PublicRoutes = ({
  component: CustomComp,
  title,
  description,
  ...rest
}) => (
  <Route
    {...rest}
    render={props => (
      <MainLayout>
        <Helmet>
          <title>freemarketone | {title}</title>
          <meta name="description" content={description} />
        </Helmet>
        <CustomComp {...props} />
      </MainLayout>
    )}
  />
);

const PrivateRoutes = ({
  component: CustomComp,
  user,
  title,
  description,
  ...rest
}) => (
  <Route
    {...rest}
    render={props =>
      user ? (
        <MainLayout>
          <Helmet>
            <title>freemarketone | {title}</title>
            <meta name="description" content={description} />
          </Helmet>
          <CustomComp {...props} />
        </MainLayout>
      ) : (
        <Redirect
          to={{
            pathname: "/login",
            state: { from: props.location }
          }}
        />
      )
    }
  />
);

const AppRouter = () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const history = useHistory();

  useEffect(() => {
    if (user !== null && history.location.pathname === "/login") {
      history.push("/");
    }
  }, [user]);

  return (
    <Suspense fallback={<Spinner />}>
      <Switch>
        {privateRoutes &&
          privateRoutes.map(route => {
            return (
              <PrivateRoutes
                key={route.id}
                path={`/${route.path}`}
                exact={route.exact}
                component={route.component}
                title={route.title}
                description={route.description}
                user={user}
              />
            );
          })}
        {publicRoutes &&
          publicRoutes.map(route => {
            return (
              <PublicRoutes
                key={route.id}
                path={`/${route.path}`}
                exact={route.exact}
                component={route.component}
                title={route.title}
                description={route.description}
              />
            );
          })}
      </Switch>
    </Suspense>
  );
};

export default AppRouter;
